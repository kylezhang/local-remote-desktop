namespace RemoteMacLAN;
public static class RemoteCoordinates {
    public static (double x,double y)? Map(double px,double py,double viewWidth,double viewHeight,int frameWidth,int frameHeight,bool clamp=false) {
        if(frameWidth<=0 || frameHeight<=0 || viewWidth<=0 || viewHeight<=0 || !double.IsFinite(px) || !double.IsFinite(py) || !double.IsFinite(viewWidth) || !double.IsFinite(viewHeight)) return null;
        double scale=Math.Min(viewWidth/frameWidth,viewHeight/frameHeight);
        double x=(px-(viewWidth-frameWidth*scale)/2)/(frameWidth*scale), y=(py-(viewHeight-frameHeight*scale)/2)/(frameHeight*scale);
        if(!clamp && (x<0 || x>1 || y<0 || y>1)) return null;
        return (Math.Clamp(x,0,1),Math.Clamp(y,0,1));
    }
}
